/**
 * This is the interpreter with the opcode switch code hastily inline into the cases to make it easier to
 * determine opcodes for games.
 *
 * It is solely for reference and is not functional code.
 */

const char *CodeBlock::exec(U32 ip, const char *functionName, Namespace *thisNamespace, U32 argc, const char **argv, bool noCalls, StringTableEntry packageName, S32 setFrame)
{
   static char traceBuffer[1024];
   U32 i;

   incRefCount();
   F64 *curFloatTable;
   char *curStringTable;
   STR.clearFunctionOffset();
   StringTableEntry thisFunctionName = NULL;
   bool popFrame = false;
   if(argv)
   {
      // assume this points into a function decl:
      U32 fnArgc = code[ip + 5];
      thisFunctionName = U32toSTE(code[ip]);
      argc = getMin(argc-1, fnArgc); // argv[0] is func name
      if(gEvalState.traceOn)
      {
         traceBuffer[0] = 0;
         dStrcat(traceBuffer, "Entering ");
         if(packageName)
         {
            dStrcat(traceBuffer, "[");
            dStrcat(traceBuffer, packageName);
            dStrcat(traceBuffer, "]");
         }
         if(thisNamespace && thisNamespace->mName)
         {
            dSprintf(traceBuffer + dStrlen(traceBuffer), sizeof(traceBuffer) - dStrlen(traceBuffer),
               "%s::%s(", thisNamespace->mName, thisFunctionName);
         }
         else
         {
            dSprintf(traceBuffer + dStrlen(traceBuffer), sizeof(traceBuffer) - dStrlen(traceBuffer),
               "%s(", thisFunctionName);
         }
         for(i = 0; i < argc; i++)
         {
            dStrcat(traceBuffer, argv[i+1]);
            if(i != argc - 1)
               dStrcat(traceBuffer, ", ");
         }
         dStrcat(traceBuffer, ")");
         Con::printf("%s", traceBuffer);
      }
      gEvalState.pushFrame(thisFunctionName, thisNamespace);
      popFrame = true;
      for(i = 0; i < argc; i++)
      {
         StringTableEntry var = U32toSTE(code[ip + i + 6]);
         gEvalState.setCurVarNameCreate(var);
         gEvalState.setStringVariable(argv[i+1]);
      }
      ip = ip + fnArgc + 6;
      curFloatTable = functionFloats;
      curStringTable = functionStrings;
   }
   else
   {
      curFloatTable = globalFloats;
      curStringTable = globalStrings;

      // Do we want this code to execute using a new stack frame?
      if (setFrame < 0)
      {
         gEvalState.pushFrame(NULL, NULL);
         popFrame = true;
      }
      else if (!gEvalState.stack.empty())
      {
         // We want to copy a reference to an existing stack frame
         // on to the top of the stack.  Any change that occurs to 
         // the locals during this new frame will also occur in the 
         // original frame.
         S32 stackIndex = gEvalState.stack.size() - setFrame - 1;
         gEvalState.pushFrameRef( stackIndex );
         popFrame = true;
      }
   }

   if (TelDebugger && setFrame < 0)
      TelDebugger->pushStackFrame();

   StringTableEntry var, objParent;
   U32 failJump;
   StringTableEntry fnName;
   StringTableEntry fnNamespace, fnPackage;
   SimObject *currentNewObject = 0;
   StringTableEntry curField;
   SimObject *curObject;
   SimObject *saveObject=NULL;
   Namespace::Entry *nsEntry;
   Namespace *ns;

   U32 callArgc;
   const char **callArgv;

   static char curFieldArray[256];

   CodeBlock *saveCodeBlock = smCurrentCodeBlock;
   smCurrentCodeBlock = this;
   if(this->name)
   {
      Con::gCurrentFile = this->name;
      Con::gCurrentRoot = mRoot;
   }
   const char * val;
   for(;;)
   {
      U32 instruction = code[ip++];
breakContinue:
      switch(instruction)
      {
         case OP_FUNC_DECL:
            if(!noCalls)
            {
               fnName       = U32toSTE(code[ip]);
               fnNamespace  = U32toSTE(code[ip+1]);
               fnPackage    = U32toSTE(code[ip+2]);
               bool hasBody = bool(code[ip+3]);
               
               Namespace::unlinkPackages();
               ns = Namespace::find(fnNamespace, fnPackage);
               ns->addFunction(fnName, this, hasBody ? ip : 0);// if no body, set the IP to 0
               Namespace::relinkPackages();

               //Con::printf("Adding function %s::%s (%d)", fnNamespace, fnName, ip);
            }
            ip = code[ip + 4];
            break;

         case OP_CREATE_OBJECT:
         {
            // If we don't allow calls, we certainly don't allow creating objects!
            if(noCalls)
            {
               ip = failJump;
               break;
            }

            // Read some useful info.
            objParent        = U32toSTE(code[ip    ]);
            bool isDataBlock =          code[ip + 1];
            failJump         =          code[ip + 2];

            // Get the constructor information off the stack.
            STR.getArgcArgv(NULL, &callArgc, &callArgv);

            // Con::printf("Creating object...");

            // objectName = argv[1]...
            currentNewObject = NULL;

            // Are we creating a datablock? If so, deal with case where we override
            // an old one.
            if(isDataBlock)
            {
               // Con::printf("  - is a datablock");

               // Find the old one if any.
               SimObject *db = Sim::getDataBlockGroup()->findObject(callArgv[2]);
               
               // Make sure we're not changing types on ourselves...
               if(db && dStricmp(db->getClassName(), callArgv[1]))
               {
                  Con::errorf(ConsoleLogEntry::General, "Cannot re-declare data block %s with a different class.", callArgv[2]);
                  ip = failJump;
                  break;
               }

               // If there was one, set the currentNewObject and move on.
               if(db)
                  currentNewObject = db;
            }

            if(!currentNewObject)
            {
               // Well, looks like we have to create a new object.
               ConsoleObject *object = ConsoleObject::create(callArgv[1]);

               // Deal with failure!
               if(!object)
               {
                  Con::errorf(ConsoleLogEntry::General, "%s: Unable to instantiate non-conobject class %s.", getFileLine(ip-1), callArgv[1]);
                  ip = failJump;
                  break;
               }

               // Do special datablock init if appropros
               if(isDataBlock)
               {
                  SimDataBlock *dataBlock = dynamic_cast<SimDataBlock *>(object);
                  if(dataBlock)
                  {
                     dataBlock->assignId();
                  }
                  else
                  {
                     // They tried to make a non-datablock with a datablock keyword!
                     Con::errorf(ConsoleLogEntry::General, "%s: Unable to instantiate non-datablock class %s.", getFileLine(ip-1), callArgv[1]);

                     // Clean up...
                     delete object;
                     ip = failJump;
                     break;
                  }
               }

               // Finally, set currentNewObject to point to the new one.
               currentNewObject = dynamic_cast<SimObject *>(object);

               // Deal with the case of a non-SimObject.
               if(!currentNewObject)
               {
                  Con::errorf(ConsoleLogEntry::General, "%s: Unable to instantiate non-SimObject class %s.", getFileLine(ip-1), callArgv[1]);
                  delete object;
                  ip = failJump;
                  break;
               }

               // Does it have a parent object? (ie, the copy constructor : syntax, not inheriance)
               if(*objParent)
               {
                  // Find it!
                  SimObject *parent;
                  if(Sim::findObject(objParent, parent))
                  {
                     // Con::printf(" - Parent object found: %s", parent->getClassName());

                     // and suck the juices from it!
                     currentNewObject->assignFieldsFrom(parent);
                  }
                  else
                     Con::errorf(ConsoleLogEntry::General, "%s: Unable to find parent object %s for %s.", getFileLine(ip-1), objParent, callArgv[1]);

                  // Mm! Juices!
               }

               // If a name was passed, assign it.
               if(callArgv[2][0])
                  currentNewObject->assignName(callArgv[2]);

               // Do the constructor parameters.
               if(!currentNewObject->processArguments(callArgc-3, callArgv+3))
               {
                  delete currentNewObject;
                  currentNewObject = NULL;
                  ip = failJump;
                  break;
               }

               // If it's not a datablock, allow people to modify bits of it.
               if(!isDataBlock)
               {
                  currentNewObject->setModStaticFields(true);
                  currentNewObject->setModDynamicFields(true);
               }
            }

            // Advance the IP past the create info...
            ip += 3;
            break;
         }

         case OP_ADD_OBJECT:
         {
            // Do we place this object at the root?
            bool placeAtRoot = code[ip++];

            // Con::printf("Adding object %s", currentNewObject->getName());

            // Make sure it wasn't already added, then add it.
            if(currentNewObject->isProperlyAdded() == false && !currentNewObject->registerObject())
            {
               // This error is usually caused by failing to call Parent::initPersistFields in the class' initPersistFields().
               Con::warnf(ConsoleLogEntry::General, "%s: Register object failed for object %s of class %s.", getFileLine(ip-2), currentNewObject->getName(), currentNewObject->getClassName());
               delete currentNewObject;
               ip = failJump;
               break;
            }

            // Are we dealing with a datablock?
            SimDataBlock *dataBlock = dynamic_cast<SimDataBlock *>(currentNewObject);
            static char errorBuffer[256];

            // If so, preload it.
            if(dataBlock && !dataBlock->preload(true, errorBuffer))
            {
               Con::errorf(ConsoleLogEntry::General, "%s: preload failed for %s: %s.", getFileLine(ip-2),
                           currentNewObject->getName(), errorBuffer);
               dataBlock->deleteObject();
               ip = failJump;
               break;
            }

            // What group will we be added to, if any?
            U32 groupAddId = intStack[UINT];
            SimGroup *grp = NULL;
            SimSet   *set = NULL;

            if(!placeAtRoot || !currentNewObject->getGroup())
            {
               if(placeAtRoot)
               {
                  // Deal with the instantGroup if we're being put at the root.
                  const char *addGroupName = Con::getVariable("instantGroup");
                  if(!Sim::findObject(addGroupName, grp))
                     Sim::findObject(RootGroupId, grp);
               }
               else
               {
                  // Otherwise just add to the requested group or set.
                  if(!Sim::findObject(groupAddId, grp))
                     Sim::findObject(groupAddId, set);
               }

               // If we didn't get a group, then make sure we have a pointer to
               // the rootgroup.
               if(!grp)
                  Sim::findObject(RootGroupId, grp);

               // add to the parent group
               grp->addObject(currentNewObject);

               // add to any set we might be in
               if(set)
                  set->addObject(currentNewObject);
            }

            // store the new object's ID on the stack (overwriting the group/set
            // id, if one was given, otherwise getting pushed)
            if(placeAtRoot) 
               intStack[UINT] = currentNewObject->getId();
            else
               intStack[++UINT] = currentNewObject->getId();

            break;
         }

         case OP_END_OBJECT:
         {
            // If we're not to be placed at the root, make sure we clean up
            // our group reference.
            bool placeAtRoot = code[ip++];
            if(!placeAtRoot)
               UINT--;
            break;
         }

         case OP_JMPIFFNOT:
            if(floatStack[FLT--])
            {
               ip++;
               break;
            }
            ip = code[ip];
            break;
         case OP_JMPIFNOT:
            if(intStack[UINT--])
            {
               ip++;
               break;
            }
            ip = code[ip];
            break;
         case OP_JMPIFF:
            if(!floatStack[FLT--])
            {
               ip++;
               break;
            }
            ip = code[ip];
            break;
         case OP_JMPIF:
            if(!intStack[UINT--])
            {
               ip ++;
               break;
            }
            ip = code[ip];
            break;
         case OP_JMPIFNOT_NP:
            if(intStack[UINT])
            {
               UINT--;
               ip++;
               break;
            }
            ip = code[ip];
            break;
         case OP_JMPIF_NP:
            if(!intStack[UINT])
            {
               UINT--;
               ip++;
               break;
            }
            ip = code[ip];
            break;
         case OP_JMP:
            ip = code[ip];
            break;
         case OP_RETURN:
            goto execFinished;
         case OP_CMPEQ:
            intStack[UINT+1] = bool(floatStack[FLT] == floatStack[FLT-1]);
            UINT++;
            FLT -= 2;
            break;

         case OP_CMPGR:
            intStack[UINT+1] = bool(floatStack[FLT] > floatStack[FLT-1]);
            UINT++;
            FLT -= 2;
            break;

         case OP_CMPGE:
            intStack[UINT+1] = bool(floatStack[FLT] >= floatStack[FLT-1]);
            UINT++;
            FLT -= 2;
            break;

         case OP_CMPLT:
            intStack[UINT+1] = bool(floatStack[FLT] < floatStack[FLT-1]);
            UINT++;
            FLT -= 2;
            break;

         case OP_CMPLE:
            intStack[UINT+1] = bool(floatStack[FLT] <= floatStack[FLT-1]);
            UINT++;
            FLT -= 2;
            break;

         case OP_CMPNE:
            intStack[UINT+1] = bool(floatStack[FLT] != floatStack[FLT-1]);
            UINT++;
            FLT -= 2;
            break;

         case OP_XOR:
            intStack[UINT-1] = intStack[UINT] ^ intStack[UINT-1];
            UINT--;
            break;

         case OP_MOD:
            intStack[UINT-1] = intStack[UINT] % intStack[UINT-1];
            UINT--;
            break;

         case OP_BITAND:
            intStack[UINT-1] = intStack[UINT] & intStack[UINT-1];
            UINT--;
            break;

         case OP_BITOR:
            intStack[UINT-1] = intStack[UINT] | intStack[UINT-1];
            UINT--;
            break;

         case OP_NOT:
            intStack[UINT] = !intStack[UINT];
            break;

         case OP_NOTF:
            intStack[UINT+1] = !floatStack[FLT];
            FLT--;
            UINT++;
            break;

         case OP_ONESCOMPLEMENT:
            intStack[UINT] = ~intStack[UINT];
            break;

         case OP_SHR:
            intStack[UINT-1] = intStack[UINT] >> intStack[UINT-1];
            UINT--;
            break;

         case OP_SHL:
            intStack[UINT-1] = intStack[UINT] << intStack[UINT-1];
            UINT--;
            break;

         case OP_AND:
            intStack[UINT-1] = intStack[UINT] && intStack[UINT-1];
            UINT--;
            break;

         case OP_OR:
            intStack[UINT-1] = intStack[UINT] || intStack[UINT-1];
            UINT--;
            break;

         case OP_ADD:
            floatStack[FLT-1] = floatStack[FLT] + floatStack[FLT-1];
            FLT--;
            break;

         case OP_SUB:
            floatStack[FLT-1] = floatStack[FLT] - floatStack[FLT-1];
            FLT--;
            break;

         case OP_MUL:
            floatStack[FLT-1] = floatStack[FLT] * floatStack[FLT-1];
            FLT--;
            break;
         case OP_DIV:
            floatStack[FLT-1] = floatStack[FLT] / floatStack[FLT-1];
            FLT--;
            break;
         case OP_NEG:
            floatStack[FLT] = -floatStack[FLT];
            break;

         case OP_SETCURVAR:
            var = U32toSTE(code[ip]);
            ip++;
            if(var[0] == '$')
               currentVariable = globalVars.lookup(var);
            else if(stack.size())
               currentVariable = stack.last()->lookup(var);
            if(!currentVariable && gWarnUndefinedScriptVariables)
               Con::warnf(ConsoleLogEntry::Script, "Variable referenced before assignment: %s", var);
            break;

         case OP_SETCURVAR_CREATE:
            var = U32toSTE(code[ip]);
            ip++;
            if(var[0] == '$')
               currentVariable = globalVars.add(var);
            else if(stack.size())
               currentVariable = stack.last()->add(var);
            else
            {
               currentVariable = NULL;
               Con::warnf(ConsoleLogEntry::Script, "Accessing local variable in global scope... failed: %s", var);
            }
            break;

         case OP_SETCURVAR_ARRAY:
            var = StringTable->insert(mBuffer + mStart);
            if(var[0] == '$')
               currentVariable = globalVars.lookup(var);
            else if(stack.size())
               currentVariable = stack.last()->lookup(var);
            if(!currentVariable && gWarnUndefinedScriptVariables)
               Con::warnf(ConsoleLogEntry::Script, "Variable referenced before assignment: %s", var);
            break;

         case OP_SETCURVAR_ARRAY_CREATE:
            var = StringTable->insert(mBuffer + mStart);
            if(var[0] == '$')
               currentVariable = globalVars.add(var);
            else if(stack.size())
               currentVariable = stack.last()->add(var);
            else
            {
               currentVariable = NULL;
               Con::warnf(ConsoleLogEntry::Script, "Accessing local variable in global scope... failed: %s", var);
            }
            break;

         case OP_LOADVAR_UINT:
            if (!currentVariable)
               val = 0;
            else if(type <= TypeInternalString)
               val = currentVariable->ival;
            else
               val = dAtoi(Con::getData(type, dataPtr, 0));
            intStack[UINT+1] = val;
            UINT++;
            break;

         case OP_LOADVAR_FLT:
            if(currentVariable->type <= TypeInternalString)
               floatStack[FLT+1] = STR.mBuffer + STR.mStart;
            else
               floatStack[FLT+1] = dAtof(Con::getData(currentVariable->type, currentVariable->dataPtr, 0));
            FLT++;
            break;

         case OP_LOADVAR_STR:
            if (!currentVariable)
               val = "";
            else if(currentVariable->type == TypeInternalString)
               val = currentVariable->sval;
            else if(currentVariable->type == TypeInternalFloat)
               val = Con::getData(TypeF32, &currentVariable->fval, 0);
            else if(currentVariable->type == TypeInternalInt)
               val = Con::getData(TypeS32, &currentVariable->ival, 0);
            else
               val = Con::getData(currentVariable->type, currentVariable->dataPtr, 0);

            if(!val)
            {
               mLen = 0;
               mBuffer[mStart] = 0;
               return;
            }
            mLen = dStrlen(val);

            validateBufferSize(mStart + mLen + 2);
            dStrcpy(mBuffer + mStart, val);
            break;

         case OP_SAVEVAR_UINT:
            val = intStack[UINT];

            if(currentVariable->type <= TypeInternalString)
            {
                currentVariable->fval = (F32)val;
                currentVariable->ival = val;
                if(sval != typeValueEmpty)
                {
                    dFree(currentVariable->sval);
                    currentVariable->sval = typeValueEmpty;
                }
                currentVariable->type = TypeInternalInt;
                return;
            }
            else
            {
                const char *dptr = Con::getData(TypeS32, &val, 0);
                Con::setData(currentVariable->type, currentVariable->dataPtr, 0, 1, &dptr);
            }
            break;

         case OP_SAVEVAR_FLT:
            val = floatStack[FLT];

            if(currentVariable->type <= TypeInternalString)
            {
               currentVariable->fval = currentVariable->val;
               currentVariable->ival = static_cast<U32>(currentVariable->val);
               if(currentVariable->sval != typeValueEmpty)
               {
                   dFree(currentVariable->sval);
                   currentVariable->sval = typeValueEmpty;
               }
               currentVariable->type = TypeInternalFloat;
               return;
            }
            else
            {
               const char *dptr = Con::getData(TypeF32, &val, 0);
               Con::setData(currentVariable->type, currentVariable->dataPtr, 0, 1, &dptr);
            }
            break;

         case OP_SAVEVAR_STR:
            val = STR.mBuffer + STR.mStart;

            if(currentVariable->type <= TypeInternalString)
            {
               U32 stringLen = dStrlen(val);

               if(stringLen < 256)
               {
                  currentVariable->fval = dAtof(val);
                  currentVariable->ival = dAtoi(val);
               }
               else
               {
                  currentVariable->fval = 0.f;
                  currentVariable->ival = 0;
               }

               currentVariable->type = TypeInternalString;

               // may as well pad to the next cache line
               U32 newLen = ((stringLen + 1) + 15) & ~15;
               
               if(currentVariable->sval == currentVariable->typeValueEmpty)
                  currentVariable->sval = (char *) dMalloc(newLen);
               else if(newLen > bufferLen)
                  currentVariable->sval = (char *) dRealloc(currentVariable->sval, newLen);

               currentVariable->bufferLen = currentVariable->newLen;
               dStrcpy(currentVariable->sval, currentVariable->val);
            }
            else
               Con::setData(currentVariable->type, currentVariable->dataPtr, 0, 1, &val);
            break;

         case OP_SETCUROBJECT:
            char *name = STR.mBuffer + STR.mStart;
            SimObject *obj;
            char c = *name;
            if(c == '/')
               return gRootGroup->findObject(name + 1 );
            if(c >= '0' && c <= '9')
            {
               // it's an id group
               const char* temp = name + 1;
               for(;;)
               {
                  c = *temp++;
                  if(!c)
                     return findObject(dAtoi(name));
                  else if(c == '/')
                  {
                     obj = findObject(dAtoi(name));
                     if(!obj)
                        return NULL;
                     return obj->findObject(temp);
                  }
               }
            }
            S32 len;

            for(len = 0; name[len] != 0 && name[len] != '/'; len++)
               ;
            StringTableEntry stName = StringTable->lookupn(name, len);
            if(!stName)
               return NULL;
            obj = gNameDictionary->find(stName);
            if(!name[len])
               return obj;
            if(!obj)
               return NULL;
            return obj->findObject(name + len + 1);

            curObject = obj;
            break;

         case OP_SETCUROBJECT_NEW:
            curObject = currentNewObject;
            break;

         case OP_SETCURFIELD:
            curField = U32toSTE(code[ip]);
            curFieldArray[0] = 0;
            ip++;
            break;

         case OP_SETCURFIELD_ARRAY:
            dStrcpy(curFieldArray, STR.mBuffer + mStart);
            break;

         case OP_LOADFIELD_UINT:
            if(curObject)
            {
               if(mFlags.test(ModStaticFields))
               {
                  S32 array1 = array ? dAtoi(array) : -1;
                  const AbstractClassRep::Field *fld = findField(slotName);
               
                  if(fld)
                  {
                     if(array1 == -1 && fld->elementCount == 1)
                        return Con::getData(fld->type, (void *) (((const char *)this) + fld->offset), 0, fld->table, fld->flag);
                     if(array1 >= 0 && array1 < fld->elementCount)
                        return Con::getData(fld->type, (void *) (((const char *)this) + fld->offset), array1, fld->table, fld->flag);// + typeSizes[fld.type] * array1));
                     return "";
                  }
               }

               if(mFlags.test(ModDynamicFields))
               {
                  if(!mFieldDictionary)
                     return "";

                  if(!array) 
                  {
                     if (const char* val = mFieldDictionary->getFieldValue(slotName))
                        return val;
                  }
                  else
                  {
                     static char buf[256];
                     dStrcpy(buf, slotName);
                     dStrcat(buf, array);
                     if (const char* val = mFieldDictionary->getFieldValue(StringTable->insert(buf)))
                        return val;
                  }
               }
               intStack[UINT+1] = U32(dAtoi(val));
            }
            else
            {
               intStack[UINT+1] = 0;
            }

            UINT++;
            break;

         case OP_LOADFIELD_FLT:
            if(curObject)
            {
               if(mFlags.test(ModStaticFields))
               {
                  S32 array1 = array ? dAtoi(array) : -1;
                  const AbstractClassRep::Field *fld = findField(slotName);
               
                  if(fld)
                  {
                     if(array1 == -1 && fld->elementCount == 1)
                        return Con::getData(fld->type, (void *) (((const char *)this) + fld->offset), 0, fld->table, fld->flag);
                     if(array1 >= 0 && array1 < fld->elementCount)
                        return Con::getData(fld->type, (void *) (((const char *)this) + fld->offset), array1, fld->table, fld->flag);// + typeSizes[fld.type] * array1));
                     return "";
                  }
               }

               if(mFlags.test(ModDynamicFields))
               {
                  if(!mFieldDictionary)
                     return "";

                  if(!array) 
                  {
                     if (const char* val = mFieldDictionary->getFieldValue(slotName))
                        return val;
                  }
                  else
                  {
                     static char buf[256];
                     dStrcpy(buf, slotName);
                     dStrcat(buf, array);
                     if (const char* val = mFieldDictionary->getFieldValue(StringTable->insert(buf)))
                        return val;
                  }
               }
               floatStack[FLT+1] = U32(dAtof(val));
            }
            else
            {
               floatStack[FLT+1] = 0;
            }
            FLT++;
            break;

         case OP_LOADFIELD_STR:
            if(curObject)
            {
               if(curObject->mFlags.test(ModStaticFields))
               {
                  S32 array1 = array ? dAtoi(array) : -1;
                  const AbstractClassRep::Field *fld = findField(slotName);
               
                  if(fld)
                  {
                     if(array1 == -1 && fld->elementCount == 1)
                        return Con::getData(fld->type, (void *) (((const char *)this) + fld->offset), 0, fld->table, fld->flag);
                     if(array1 >= 0 && array1 < fld->elementCount)
                        return Con::getData(fld->type, (void *) (((const char *)this) + fld->offset), array1, fld->table, fld->flag);// + typeSizes[fld.type] * array1));
                     return "";
                  }
               }

               if(curObject->mFlags.test(ModDynamicFields))
               {
                  if(!mFieldDictionary)
                     val = "";
                  else if(!array) 
                  {
                     if (const char* val = curObject->mFieldDictionary->getFieldValue(slotName))
                        return val;
                  }
                  else
                  {
                     static char buf[256];
                     dStrcpy(buf, slotName);
                     dStrcat(buf, array);
                     if (const char* val = curObject->mFieldDictionary->getFieldValue(StringTable->insert(buf)))
                        return val;
                  }
               }

               return "";
               val = curObject->getDataField(curField, curFieldArray);
            }
            else
            {
               val = "";
            }

            if(!val)
            {
               STR.mLen = 0;
               STR.mBuffer[STR.mStart] = 0;
               return;
            }

            STR.mLen = dStrlen(val);

            validateBufferSize(STR.mStart + STR.mLen + 2);
            dStrcpy(STR.mBuffer + STR.mStart, val);
            break;

         case OP_SAVEFIELD_UINT:
            validateBufferSize(mStart + 32);
            dSprintf(mBuffer + mStart, 32, "%d", intStack[UINT]);
            mLen = dStrlen(mBuffer + mStart);
            if(curObject)
               curObject->setDataField(curField, curFieldArray, STR.getStringValue());
            break;

         case OP_SAVEFIELD_FLT:
            validateBufferSize(mStart + 32);
            dSprintf(mBuffer + mStart, 32, "%g", floatStack[FLT]);
            mLen = dStrlen(mBuffer + mStart);
            if(curObject)
               curObject->setDataField(curField, curFieldArray, STR.getStringValue());
            break;

         case OP_SAVEFIELD_STR:
            if(curObject)
               curObject->setDataField(curField, curFieldArray, STR.getStringValue());
            break;

         case OP_STR_TO_UINT:
            intStack[UINT+1] = dAtoi(STR.mBuffer + STR.mStart)
            UINT++;
            break;

         case OP_STR_TO_FLT:
            floatStack[FLT+1] = dAtof(STR.mBuffer + STR.mStart)
            FLT++;
            break;

         case OP_STR_TO_NONE:
            // This exists simply to deal with certain typecast situations.
            break;

         case OP_FLT_TO_UINT:
            intStack[UINT+1] = (unsigned int)floatStack[FLT];
            FLT--;
            UINT++;
            break;

         case OP_FLT_TO_STR:
            validateBufferSize(STR.mStart + 32);
            dSprintf(STR.mBuffer + STR.mStart, 32, "%g", floatStack[FLT]);
            STR.mLen = dStrlen(STR.mBuffer + STR.mStart);
            FLT--;
            break;

         case OP_FLT_TO_NONE:
            FLT--;
            break;

         case OP_UINT_TO_FLT:
            floatStack[FLT+1] = intStack[UINT];
            UINT--;
            FLT++;
            break;

         case OP_UINT_TO_STR:
            validateBufferSize(STR.mStart + 32);
            dSprintf(STR.mBuffer + STR.mStart, 32, "%d", intStack[UINT]);
            STR.mLen = dStrlen(STR.mBuffer + STR.mStart);
            UINT--;
            break;

         case OP_UINT_TO_NONE:
            UINT--;
            break;

         case OP_LOADIMMED_UINT:
            intStack[UINT+1] = code[ip++];
            UINT++;
            break;

         case OP_LOADIMMED_FLT:
            floatStack[FLT+1] = curFloatTable[code[ip]];
            ip++;
            FLT++;
            break;
         case OP_TAG_TO_STR:
            code[ip-1] = OP_LOADIMMED_STR;
            // it's possible the string has already been converted
            if(U8(curStringTable[code[ip]]) != StringTagPrefixByte)
            {
               U32 id = GameAddTaggedString(curStringTable + code[ip]);
               dSprintf(curStringTable + code[ip] + 1, 7, "%d", id);
               *(curStringTable + code[ip]) = StringTagPrefixByte;
            }
         case OP_LOADIMMED_STR:
            val = curStringTable + code[ip++];
            if(!val)
            {
               STR.mLen = 0;
               STR.mBuffer[STR.mStart] = 0;
               return;
            }
            STR.mLen = dStrlen(val);

            validateBufferSize(STR.mStart + STR.mLen + 2);
            dStrcpy(STR.mBuffer + STR.mStart, val);
            break;

         case OP_LOADIMMED_IDENT:
            val = U32toSTE(code[ip++]);
            if(!val)
            {
               STR.mLen = 0;
               STR.mBuffer[STR.mStart] = 0;
               return;
            }
            STR.mLen = dStrlen(val);

            validateBufferSize(STR.mStart + STR.mLen + 2);
            dStrcpy(STR.mBuffer + STR.mStart, val);
            break;

         case OP_CALLFUNC_RESOLVE:
            // This deals with a function that is potentially living in a namespace.
            fnNamespace = U32toSTE(code[ip+1]);
            fnName      = U32toSTE(code[ip]);

            // Try to look it up.
            ns = Namespace::find(fnNamespace);
            nsEntry = ns->lookup(fnName);
            if(!nsEntry)
            {
               ip+= 3;
               Con::warnf(ConsoleLogEntry::General,
                  "%s: Unable to find function %s%s%s",
                  getFileLine(ip-4), fnNamespace ? fnNamespace : "",
                  fnNamespace ? "::" : "", fnName);
               STR.getArgcArgv(fnName, &callArgc, &callArgv);
               break;
            }
            // Now, rewrite our code a bit (ie, avoid future lookups) and fall
            // through to OP_CALLFUNC
            code[ip+1] = *((U32 *) &nsEntry);
            code[ip-1] = OP_CALLFUNC;

         case OP_CALLFUNC:
         {
            fnName = U32toSTE(code[ip]);

            //if this is called from inside a function, append the ip and codeptr
            if (!gEvalState.stack.empty())
            {
               gEvalState.stack.last()->code = this;
               gEvalState.stack.last()->ip = ip - 1;
            }

            U32 callType = code[ip+2];

            ip += 3;
            STR.getArgcArgv(fnName, &callArgc, &callArgv);

            if(callType == FuncCallExprNode::FunctionCall) {
               nsEntry = *((Namespace::Entry **) &code[ip-2]);
               ns = NULL;
            }
            else if(callType == FuncCallExprNode::MethodCall)
            {
               saveObject = gEvalState.thisObject;
               gEvalState.thisObject = Sim::findObject(callArgv[1]);
               if(!gEvalState.thisObject)
               {
                  gEvalState.thisObject = 0;
                  Con::warnf(ConsoleLogEntry::General,"%s: Unable to find object: '%s' attempting to call function '%s'", getFileLine(ip-4), callArgv[1], fnName);
                  break;
               }
               ns = gEvalState.thisObject->getNamespace();
               if(ns)
                  nsEntry = ns->lookup(fnName);
               else
                  nsEntry = NULL;
            }
            else // it's a ParentCall
            {
               if(thisNamespace)
               {
                  ns = thisNamespace->mParent;
                  if(ns)
                     nsEntry = ns->lookup(fnName);
                  else
                     nsEntry = NULL;
               }
               else
               {
                  ns = NULL;
                  nsEntry = NULL;
               }
            }

            if(!nsEntry || noCalls)
            {
               if(!noCalls)
               {
                  Con::warnf(ConsoleLogEntry::General,"%s: Unknown command %s.", getFileLine(ip-4), fnName);
                  if(callType == FuncCallExprNode::MethodCall)
                  {
                     Con::warnf(ConsoleLogEntry::General, "  Object %s(%d) %s",
                           gEvalState.thisObject->getName() ? gEvalState.thisObject->getName() : "",
                           gEvalState.thisObject->getId(), getNamespaceList(ns) );
                  }
               }
               STR.setStringValue("");
               break;
            }
            if(nsEntry->mType == Namespace::Entry::ScriptFunctionType)
            {
               if(nsEntry->mFunctionOffset)
                  nsEntry->mCode->exec(nsEntry->mFunctionOffset, fnName, nsEntry->mNamespace, callArgc, callArgv, false, nsEntry->mPackage);
               else // no body
                  STR.setStringValue("");
            }
            else
            {
               if((nsEntry->mMinArgs && S32(callArgc) < nsEntry->mMinArgs) || (nsEntry->mMaxArgs && S32(callArgc) > nsEntry->mMaxArgs))
               {
                  const char* nsName = ns? ns->mName: "";
                  Con::warnf(ConsoleLogEntry::Script, "%s: %s::%s - wrong number of arguments.", getFileLine(ip-4), nsName, fnName);
                  Con::warnf(ConsoleLogEntry::Script, "%s: usage: %s", getFileLine(ip-4), nsEntry->mUsage);
               }
               else
               {
                  switch(nsEntry->mType)
                  {
                     case Namespace::Entry::StringCallbackType:
                     {
                        const char *ret = nsEntry->cb.mStringCallbackFunc(gEvalState.thisObject, callArgc, callArgv);
                        if(ret != STR.getStringValue())
                           STR.setStringValue(ret);
                        else
                           STR.setLen(dStrlen(ret));
                        break;
                     }
                     case Namespace::Entry::IntCallbackType:
                     {
                        S32 result = nsEntry->cb.mIntCallbackFunc(gEvalState.thisObject, callArgc, callArgv);
                        if(code[ip] == OP_STR_TO_UINT)
                        {
                           ip++;
                           intStack[++UINT] = result;
                           break;
                        }
                        else if(code[ip] == OP_STR_TO_FLT)
                        {
                           ip++;
                           floatStack[++FLT] = result;
                           break;
                        }
                        else if(code[ip] == OP_STR_TO_NONE)
                           ip++;
                        else
                           STR.setIntValue(result);
                        break;
                     }
                     case Namespace::Entry::FloatCallbackType:
                     {
                        F64 result = nsEntry->cb.mFloatCallbackFunc(gEvalState.thisObject, callArgc, callArgv);
                        if(code[ip] == OP_STR_TO_UINT)
                        {
                           ip++;
                           intStack[++UINT] = (unsigned int)result;
                           break;
                        }
                        else if(code[ip] == OP_STR_TO_FLT)
                        {
                           ip++;
                           floatStack[++FLT] = result;
                           break;
                        }
                        else if(code[ip] == OP_STR_TO_NONE)
                           ip++;
                        else
                           STR.setFloatValue(result);
                        break;
                     }
                     case Namespace::Entry::VoidCallbackType:
                        nsEntry->cb.mVoidCallbackFunc(gEvalState.thisObject, callArgc, callArgv);
                        if(code[ip] != OP_STR_TO_NONE)
                           Con::warnf(ConsoleLogEntry::General, "%s: Call to %s in %s uses result of void function call.", getFileLine(ip-4), fnName, functionName);
                        STR.setStringValue("");
                        break;
                     case Namespace::Entry::BoolCallbackType:
                     {
                        bool result = nsEntry->cb.mBoolCallbackFunc(gEvalState.thisObject, callArgc, callArgv);
                        if(code[ip] == OP_STR_TO_UINT)
                        {
                           ip++;
                           intStack[++UINT] = result;
                           break;
                        }
                        else if(code[ip] == OP_STR_TO_FLT)
                        {
                           ip++;
                           floatStack[++FLT] = result;
                           break;
                        }
                        else if(code[ip] == OP_STR_TO_NONE)
                           ip++;
                        else
                           STR.setIntValue(result);
                        break;
                     }
                  }
               }
            }

            if(callType == FuncCallExprNode::MethodCall)
               gEvalState.thisObject = saveObject;
            break;
         }
         case OP_ADVANCE_STR:
            STR.mStartOffsets[STR.mStartStackSize++] = STR.mStart;
            STR.mStart += STR.mLen;
            STR.mLen = 0;
            break;
         case OP_ADVANCE_STR_APPENDCHAR:
            STR.mStartOffsets[STR.mStartStackSize++] = STR.mStart;
            STR.mStart += STR.mLen;
            STR.mBuffer[STR.mStart] = code[ip++];
            STR.mBuffer[STR.mStart+1] = 0;
            STR.mStart += 1;
            STR.mLen = 0;
            break;

         case OP_ADVANCE_STR_COMMA:
            STR.mStartOffsets[STR.mStartStackSize++] = STR.mStart;
            STR.mStart += STR.mLen;
            STR.mBuffer[STR.mStart] = code[ip++];
            STR.mBuffer[STR.mStart+1] = '_';
            STR.mStart += 1;
            STR.mLen = 0;
            break;

         case OP_ADVANCE_STR_NUL:
            STR.mStartOffsets[STR.mStartStackSize++] = STR.mStart;
            STR.mStart += STR.mLen;
            STR.mBuffer[STR.mStart] = code[ip++];
            STR.mBuffer[STR.mStart+1] = '\0';
            STR.mStart += 1;
            STR.mLen = 0;
            break;

         case OP_REWIND_STR:
            STR.mStart = STR.mStartOffsets[--STR.mStartStackSize];
            STR.mLen = dStrlen(STR.mBuffer + STR.mStart);
            break;

         case OP_TERMINATE_REWIND_STR:
            STR.mBuffer[STR.mStart] = 0;
            STR.mStart = STR.mStartOffsets[--STR.mStartStackSize];
            STR.mLen   = dStrlen(STR.mBuffer + STR.mStart);
            break;

         case OP_COMPARE_STR:
            U32 oldStart = STR.mStart;
            STR.mStart = STR.mStartOffsets[--STR.mStartStackSize];

            U32 ret = !dStricmp(STR.mBuffer + STR.mStart, STR.mBuffer + oldStart);

            STR.mLen = 0;
            STR.mBuffer[STR.mStart] = 0;

            intStack[++UINT] = ret;
            break;

         case OP_PUSH:
            STR.mStartOffsets[STR.mStartStackSize++] = STR.mStart;
            STR.mStart += STR.mLen;
            STR.mBuffer[STR.mStart] = code[ip++];
            STR.mBuffer[STR.mStart+1] = '\0';
            STR.mStart += 1;
            STR.mLen = 0;
            break;

         case OP_PUSH_FRAME:
            STR.mFrameOffsets[STR.mNumFrames++] = STR.mStartStackSize;
            STR.mStartOffsets[STR.mStartStackSize++] = STR.mStart;
            STR.mStart += 512;
            validateBufferSize(0);
            break;

         case OP_BREAK:
         {
            //append the ip and codeptr before managing the breakpoint!
            AssertFatal( !gEvalState.stack.empty(), "Empty eval stack on break!");
            gEvalState.stack.last()->code = this;
            gEvalState.stack.last()->ip = ip - 1;

            U32 breakLine;
            findBreakLine(ip-1, breakLine, instruction);
            if(!breakLine)
               goto breakContinue;
            TelDebugger->executionStopped(this, breakLine);
            goto breakContinue;
         }
         case OP_INVALID:

         default:
            // error!
            goto execFinished;
      }
   }
execFinished:

   if (TelDebugger && setFrame < 0)
      TelDebugger->popStackFrame();

   if ( popFrame )
      gEvalState.popFrame();

   if(argv)
   {
      if(gEvalState.traceOn)
      {
         traceBuffer[0] = 0;
         dStrcat(traceBuffer, "Leaving ");

         if(packageName)
         {
            dStrcat(traceBuffer, "[");
            dStrcat(traceBuffer, packageName);
            dStrcat(traceBuffer, "]");
         }
         if(thisNamespace && thisNamespace->mName)
         {
            dSprintf(traceBuffer + dStrlen(traceBuffer), sizeof(traceBuffer) - dStrlen(traceBuffer),
               "%s::%s() - return %s", thisNamespace->mName, thisFunctionName, STR.getStringValue());
         }
         else
         {
            dSprintf(traceBuffer + dStrlen(traceBuffer), sizeof(traceBuffer) - dStrlen(traceBuffer),
               "%s() - return %s", thisFunctionName, STR.getStringValue());
         }
         Con::printf("%s", traceBuffer);
      }
   }
   else
   {
      delete[] const_cast<char*>(globalStrings);
      delete[] globalFloats;
      globalStrings = NULL;
      globalFloats = NULL;
   }
   smCurrentCodeBlock = saveCodeBlock;
   if(saveCodeBlock && saveCodeBlock->name)
   {
      Con::gCurrentFile = saveCodeBlock->name;
      Con::gCurrentRoot = saveCodeBlock->mRoot;
   }

   decRefCount();
   return STR.getStringValue();
}
